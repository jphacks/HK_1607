class StudentWarningJob < ApplicationJob
  queue_as :default

  # 理解度の値を閾値を下回った生徒に対する通知
  def perform(userId, state)
    # 対象の生徒に対して通知
    LearningChannel.broadcast_to(User.find_by(login_id: userId), {
      # 現状は送信する状態はwarkingのみ
      state: state
    })
  end
end
