class StudentsController < ApplicationController
  before_action :access_filter

  def index
  end

  private
    def access_filter
      if current_user.teacher_flag
        # 先生でログイン
        redirect_to teacher_url
      else
        # 生徒でログイン
      end
    end
end
